using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GenerateProvider]
    public class GenerateBakerAndComponentActionWorkflowProvider : IGenerateWorkflowProvider
    {
        public IEnumerable<IGenerateActionWorkflow> CreateWorkflow(IDataContext dataContext)
        {
            return new[] {new GenerateBakerAndComponentActionWorkflow()};
        }
    }
}