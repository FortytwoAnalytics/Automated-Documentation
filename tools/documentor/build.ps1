param (
    [Parameter(Mandatory = $false)][string]$solution
)

## Available in root path based on standard install script for hosted instance
$CMD = (get-location).path + '\tools\tabulareditor\TabularEditor.exe'
Write-Host $CMD

#Source folder used for deployment
$SSASPath = (Join-path -Path (get-location).Path -ChildPath "AAS")

if ($solution -ne "") 
{
	$SSASPath = (Join-path -Path $SSASPath -ChildPath $solution)
}
# Excluded for now since we only have 1 model
#$arg1 = (Join-Path -Path $SSASPath -ChildPath $ModelName )
$arg1 = "`"$SSASPath`""
Write-Host $arg1

#Ensuring that errors and warnings are outputtet in a format transparent for the VSTS build engine
$arg4 = '-v'
Write-Host $arg3

## Build new file4
Write-Host "Full command:"
Write-Host "`"$CMD`" $arg1 $arg2 $arg3"

    $scriptFile = (get-location).path + '\tools\documentor\\tabular-to-markdown.cs'
    #File used for customer build of model for country
    $arg2 = '-S "' + $scriptFile + '"'
    Write-Host $arg2

cmd /c "`"$CMD`" $arg1 $arg2 $arg3"

Write-Host "Build completed..."