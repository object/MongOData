# Install.ps1
param($installPath, $toolsPath, $package, $project)

# remove deprecated assembly references
$project.Object.References | Where-Object { $_.Name -eq 'System.Data.Services' } | ForEach-Object { $_.Remove() }
$project.Object.References | Where-Object { $_.Name -eq 'System.Data.Services.Client' } | ForEach-Object { $_.Remove() }

# remove duplicate connection strings

$xml = New-Object xml

# find the Web.config file
$config = $project.ProjectItems | where {$_.Name -eq "Web.config"}

# find its path on the file system
$localPath = $config.Properties | where {$_.Name -eq "LocalPath"}

# load Web.config as XML
$xml.Load($localPath.Value)

# select the node
$parent = $xml.SelectSingleNode("configuration/connectionStrings")
$nodes = $parent.SelectNodes("add[@name='MongoDB']")

# remove duplicate nodes
$first = $true
foreach ($node in $nodes)
{
	if (!$first) 
	{
		$parent.RemoveChild($node)
	}
	$first = $false
}

# save the Web.config file
$xml.Save($localPath.Value)
