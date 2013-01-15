param($installPath, $toolsPath, $package, $project)

Write-Host $("==== BEGIN PlexCMS Post-Install Script");

$assemblyPath = [System.IO.Path]::Combine($toolsPath, "Plex.dll");
$plexAssembly = [Reflection.Assembly]::LoadFrom($assemblyPath);
$projectPath = [System.IO.Path]::GetDirectoryName($project.FullName);
[Plex.Web.Installer]::Install($installPath, $projectPath);

Write-Host $("==== END PlexCMS Post-Install Script");
