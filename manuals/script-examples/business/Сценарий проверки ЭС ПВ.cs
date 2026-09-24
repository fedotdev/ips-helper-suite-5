using System;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using Intermech.Interfaces;
using Intermech.Interfaces.Client;

public class Script
{
    public ICSharpScriptContext ScriptContext { get; set; }

    public AttributeValidationScriptParameters Execute(AttributeValidationScriptParameters parameters)
    {
        MessageBox.Show("Проверка ЭС производственной ведомости настраивается согласно индивидуальных требований предприятия, силами самого предприятия.");

        return parameters;
    }
}