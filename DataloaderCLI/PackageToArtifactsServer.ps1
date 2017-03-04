#create folder called DataLoaderCLI_%VERSION%
$NewFolderName = 'DataLoaderCLI_'+$args[0]

$FolderFUllPath = '\\sec-it-artifacts\Software\DataLoaderCLI\'+$NewFolderName

New-Item $FolderFUllPath -ItemType Directory


Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\*.dll $FolderFUllPath
Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\DataloaderCLI.exe $FolderFUllPath
Copy-Item DataLoaderCLI\DataloaderCLI\conf.ini $FolderFUllPath
