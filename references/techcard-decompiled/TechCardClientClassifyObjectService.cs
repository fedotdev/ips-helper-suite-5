using System.Collections.Generic;
using System.Threading;
using Intermech.Diagnostics;
using Intermech.Expert.User;
using Intermech.Interfaces;
using Intermech.Interfaces.Expert;
using Intermech.Interfaces.TechCard.Services.ClassifyObject;
using Intermech.Memento;

namespace Intermech.TechCard.Client.Services.ClassifyObject;

internal class TechCardClientClassifyObjectService : ITechCardClientClassifyObjectService
{
	public IList<AttributeValues> ClassifyObject([NotNull] IUserSession session, [NotNull] TechCardClassifyObjectParams classifyObjectParams, ICollection<ITechCardClassifyObjectStrategy> classifyStrategies)
	{
		ITechCardClassifyObjectService service = ServiceUtils.GetService<ITechCardClassifyObjectService>(session, throwIfNotFound: true);
		if (!classifyObjectParams.UseExpertService || classifyObjectParams.ExpertTaskId != 0)
		{
			return service.ClassifyObject(session.SessionGUID, classifyObjectParams, classifyStrategies);
		}
		IExpertUser service2 = ServiceUtils.GetService<IExpertUser>(ApplicationServices.Container, throwIfNotFound: true);
		IExpertServer service3 = ServiceUtils.GetService<IExpertServer>(session, throwIfNotFound: true);
		int num = service3.StartTask(session.SessionGUID, ExpertTraceFlags.None);
		try
		{
			service3.SetDateTimeFormat(num, new DateTimeFormatInfoMemento(Thread.CurrentThread.CurrentCulture.DateTimeFormat));
			service3.SetNumberFormat(num, new NumberFormatInfoMemento(Thread.CurrentThread.CurrentCulture.NumberFormat));
			service3.SetTrace(num, service2.ShowTraceWindow);
			service3.SetLog(num, service2.ReportLog);
			service3.SetTraceFlags(num, ExpertTask.GetConfTraceFlags());
			classifyObjectParams.ExpertTaskId = num;
			IList<AttributeValues> result = service.ClassifyObject(session.SessionGUID, classifyObjectParams, classifyStrategies);
			classifyObjectParams.ExpertTaskId = 0;
			if (service2.ShowTraceWindow)
			{
				ExpertUser.rur.Execute(service3.GetTraceInfo(num), appendMode: true);
			}
			return result;
		}
		finally
		{
			service3.EndTask(num);
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '8.2.0.7535-95108c96')
