param($installPath, $toolsPath, $package, $project)

Write-Host $("==== BEGIN PlexCMS Pre-Install Script");

$removals = @{
	"Controllers" = "HomeController.cs";
	"Views" = "_ViewStart.cshtml";
	"App_Start" = "RouteConfig.cs,WebApiConfig.cs";
	"Home" = "About.cshtml,Contact.cshtml,Index.cshtml";
};

function AttemptRemovals($folder) {
	if (($folder.Kind -eq "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}") -or ($folder.Kind -eq "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")) {
		foreach ($item in $removals.GetEnumerator()) {
			if ($folder.Name -eq $item.Key) {
				$filenames = $item.Value.Split(",");
				foreach ($file in $folder.ProjectItems) {
					foreach ($filename in $filenames) {
						if ($file.Name -eq $filename) {
							Write-Host $("Removing: " + $folder.Name + "/" + $file.Name);
							$file.Delete();
						}
					}
				}
			}
		}
	}
}

foreach ($folder in $project.ProjectItems) {
	AttemptRemovals($folder);
	foreach ($subfolder in $folder.ProjectItems) {
		AttemptRemovals($subfolder);
	}
}

Write-Host $("==== END PlexCMS Pre-Install Script");
