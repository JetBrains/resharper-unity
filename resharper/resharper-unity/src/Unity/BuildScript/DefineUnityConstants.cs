using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.PreCompile.Autofix;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.Unity.BuildScript
{
	public static class DefineUnityConstants
	{
		[BuildStep]
		public static IEnumerable<AutofixAllowedDefineConstant> YieldAllowedDefineConstantsForMstest()
		{
			var constants = new List<string>();

			constants.AddRange(new[] {"INDEPENDENT_BUILD"});

			return constants.SelectMany(s => new []
			{
				new AutofixAllowedDefineConstant(new SubplatformName("Plugins\\ReSharperUnity\\resharper\\resharper-unity\\test\\src"), s)
            });
		}
	}
}