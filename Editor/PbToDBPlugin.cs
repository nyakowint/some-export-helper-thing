using nadena.dev.ndmf;
using Nyako.ExportHelper.Editor;

[assembly: ExportsPlugin(typeof(PbToDBPlugin))]

namespace Nyako.ExportHelper.Editor
{
    [RunsOnAllPlatforms]
    public class PbToDBPlugin : Plugin<PbToDBPlugin>
    {
        public override string QualifiedName => "cat.nyako.export-helper";
        public override string DisplayName => "PhysBones → DynamicBones (Beat Saber Export)";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(new PbToDBPass());
        }
    }
}
