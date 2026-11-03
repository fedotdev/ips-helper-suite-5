/**
 * Session result files on disk.
 *
 * The portal serves the files of `output/<session id>` straight from the file
 * system - OpenCode's own file API is never used. Every download goes through
 * a strict allow list:
 *
 *   - the request may only carry a plain file name (no directories at all),
 *   - `..`, absolute paths, UNC paths, drive letters and NUL bytes are rejected,
 *   - Windows reserved device names are rejected,
 *   - symbolic links are rejected (both the entry itself and a link that points
 *     outside of the session directory),
 *   - the extension must be in `files.allowedExtensions`,
 *   - the file must be a regular file inside the session directory and must not
 *     exceed `files.maxBytes`.
 */

import { lstatSync, readdirSync, realpathSync } from "node:fs"
import { basename, extname, isAbsolute, join, relative, resolve, sep, win32 } from "node:path"

export interface FileEntry {
  name: string
  size: number
  modifiedAt: number
}

export type FileRejection =
  | "name-empty"
  | "name-path"
  | "name-traversal"
  | "name-absolute"
  | "name-reserved"
  | "name-invalid"
  | "not-found"
  | "not-a-file"
  | "symlink"
  | "outside"
  | "extension"
  | "too-large"

export class FileAccessError extends Error {
  readonly reason: FileRejection

  constructor(reason: FileRejection, message: string) {
    super(message)
    this.name = "FileAccessError"
    this.reason = reason
  }
}

const RESERVED = new Set([
  "con",
  "prn",
  "aux",
  "nul",
  "com1",
  "com2",
  "com3",
  "com4",
  "com5",
  "com6",
  "com7",
  "com8",
  "com9",
  "lpt1",
  "lpt2",
  "lpt3",
  "lpt4",
  "lpt5",
  "lpt6",
  "lpt7",
  "lpt8",
  "lpt9",
])

export interface FileRules {
  allowedExtensions: string[]
  maxBytes: number
}

/** True when `name` is a plain file name that cannot escape its directory. */
export function isSafeFileName(name: string): boolean {
  if (typeof name !== "string" || name.length === 0 || name.length > 255) return false
  if (name.includes("\0")) return false
  if (name.includes("/") || name.includes("\\")) return false
  if (name === "." || name === "..") return false
  if (isAbsolute(name) || win32.isAbsolute(name)) return false
  if (/^[A-Za-z]:/.test(name)) return false
  // Windows trailing dot / space are silently stripped - reject them.
  if (/[. ]$/.test(name)) return false
  if (/[<>:"|?*]/.test(name)) return false
  if (name.startsWith(" ")) return false
  const stem = name.split(".")[0]?.toLowerCase() ?? ""
  if (RESERVED.has(stem)) return false
  if (name.split(".").length < 2) return false
  return true
}

/** Extension check against the allow list (without the dot, lower case). */
export function hasAllowedExtension(name: string, rules: FileRules): boolean {
  const ext = extname(name).replace(/^\./, "").toLowerCase()
  if (!ext) return false
  return rules.allowedExtensions.includes(ext)
}

/**
 * Resolve a file inside `dir`.
 * @throws FileAccessError with a machine readable reason.
 */
export function resolveSessionFile(dir: string, name: string, rules: FileRules): { path: string; size: number } {
  if (typeof name !== "string" || name.length === 0) {
    throw new FileAccessError("name-empty", "file name is required")
  }
  if (name.includes("\0")) {
    throw new FileAccessError("name-invalid", "file name contains an invalid character")
  }
  if (name.includes("/") || name.includes("\\")) {
    throw new FileAccessError("name-path", "file name must not contain a path")
  }
  if (name === "." || name === "..") {
    throw new FileAccessError("name-traversal", "file name must not contain a path")
  }
  if (isAbsolute(name) || win32.isAbsolute(name) || /^[A-Za-z]:/.test(name)) {
    throw new FileAccessError("name-absolute", "file name must be relative")
  }
  if (!isSafeFileName(name)) {
    throw new FileAccessError("name-invalid", "file name is not allowed")
  }
  if (!hasAllowedExtension(name, rules)) {
    throw new FileAccessError("extension", `file extension is not allowed (allowed: ${rules.allowedExtensions.join(", ")})`)
  }

  const root = resolve(dir)
  const target = resolve(join(root, name))
  const rel = relative(root, target)
  if (rel === "" || rel.startsWith("..") || isAbsolute(rel) || rel.split(sep).includes("..")) {
    throw new FileAccessError("outside", "file is outside of the session directory")
  }
  if (basename(target) !== name) {
    throw new FileAccessError("name-path", "file name must not contain a path")
  }

  let stat: ReturnType<typeof lstatSync>
  try {
    stat = lstatSync(target)
  } catch {
    throw new FileAccessError("not-found", "file not found")
  }
  if (stat.isSymbolicLink()) {
    throw new FileAccessError("symlink", "symbolic links are not served")
  }
  if (!stat.isFile()) {
    throw new FileAccessError("not-a-file", "not a regular file")
  }

  // A hard link or a junction inside the directory cannot be detected by lstat
  // alone, so the resolved real path must still stay inside the session folder.
  try {
    const realRoot = realpathSync(root)
    const realTarget = realpathSync(target)
    const realRel = relative(realRoot, realTarget)
    if (realRel === "" || realRel.startsWith("..") || isAbsolute(realRel)) {
      throw new FileAccessError("outside", "file resolves outside of the session directory")
    }
  } catch (err) {
    if (err instanceof FileAccessError) throw err
    throw new FileAccessError("not-found", "file not found")
  }

  if (stat.size > rules.maxBytes) {
    throw new FileAccessError("too-large", "file is larger than the allowed size")
  }
  return { path: target, size: stat.size }
}

/** List the downloadable files of one session directory. */
export function listSessionFiles(dir: string, rules: FileRules): FileEntry[] {
  let entries: ReturnType<typeof readdirSync>
  try {
    entries = readdirSync(dir, { withFileTypes: true })
  } catch {
    return []
  }
  const files: FileEntry[] = []
  for (const entry of entries) {
    if (!entry.isFile()) continue
    if (!hasAllowedExtension(entry.name, rules)) continue
    try {
      const stat = lstatSync(join(dir, entry.name))
      if (!stat.isFile() || stat.isSymbolicLink()) continue
      if (stat.size > rules.maxBytes) continue
      files.push({ name: entry.name, size: stat.size, modifiedAt: Math.floor(stat.mtimeMs) })
    } catch {
      /* unreadable entry - skip */
    }
  }
  files.sort((a, b) => a.name.localeCompare(b.name))
  return files
}
