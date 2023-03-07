using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.PreCompile.Autofix;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.Json.Tests.BuildScript
{
	public static class DefineUnityJsonTestsConstants
	{
		[BuildStep]
		public static IEnumerable<AutofixAllowedDefineConstant> YieldAllowedDefineConstantsForUnity()
		{
			var constants = new List<string>();

			constants.AddRange(new[] { "$(DefineConstants)", "JET_MODE_ASSERT", "JET_MODE_REPORT_EXCEPTIONS" } );

			return constants.SelectMany(s => new []
			{
				new AutofixAllowedDefineConstant(new SubplatformName("Plugins\\ReSharperUnity\\resharper\\resharper-json\\test\\src"), s),
			});
		}
	}
}