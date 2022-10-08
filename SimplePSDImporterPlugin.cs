#if TOOLS
using Godot;

[Tool]
public partial class SimplePSDImporterPlugin : EditorPlugin {

	SimplePSDImporter importer;
	public override void _EnterTree() {
		importer = new SimplePSDImporter();
		AddImportPlugin(importer);
	}

	public override void _ExitTree() {
		RemoveImportPlugin(importer);
	}
}
#endif


