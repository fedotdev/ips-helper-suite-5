using System.Collections.Generic;
using Intermech.Diagnostics;
using Intermech.Interfaces;
using Intermech.Interfaces.TechCard.Services.ClassifyObject;

namespace Intermech.TechCard.Client.Services.ClassifyObject;

public static class TechCardClientClassifyObjectHelper
{
	public static IList<AttributeValues> ClassifyObject([NotNull] IUserSession session, [NotNull] TechCardClassifyObjectParams classifyBaseObjectParams, ICollection<ITechCardClassifyObjectStrategy> classifyStrategies = null)
	{
		return ServiceUtils.GetService<ITechCardClientClassifyObjectService>(ApplicationServices.Container, throwIfNotFound: true).ClassifyObject(session, classifyBaseObjectParams, classifyStrategies);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '8.2.0.7535-95108c96')
