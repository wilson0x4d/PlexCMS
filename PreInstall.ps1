param($installPath, $toolsPath, $package, $project)

Write-Host $("==== BEGIN PlexCMS Pre-Install Script");

$removals = @{
	"Controllers" = "HomeController.cs";
	"Views" = "_ViewStart.cshtml";
	"Layouts" = "_Classic.cshtml,_Orange.cshtml,_Purple.cshtml";
	"App_Start" = "RouteConfig.cs,WebApiConfig.cs";
	"Home" = "About.cshtml,Contact.cshtml,Index.cshtml";
	"Areas" = "PlexAdmin";
};

function AttemptRemovals($folder) {
	if (($folder.Kind -eq "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}") -or ($folder.Kind -eq "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")) {
		foreach ($item in $removals.GetEnumerator()) {
			if (($folder.Name -eq $item.Key) -or ($item.Key -eq "")) {
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

function AttemptRemovalsRecursive($folder) {
	foreach ($subFolder in $folder.ProjectItems) {
		AttemptRemovals($subFolder);
		AttemptRemovalsRecursive($subFolder);
	}
}

AttemptRemovalsRecursive($project);

Write-Host $("==== END PlexCMS Pre-Install Script");
