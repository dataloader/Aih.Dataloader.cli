#create folder called DataLoaderCLI_%VERSION%
$NewFolderName = 'DataLoaderCLI_'+$args[0]

$FolderFUllPath = '\\sec-it-artifacts\Software\'+$NewFolderName

New-Item $FolderFUllPath -ItemType Directory


Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\Aih.Dataloader.dll $FolderFUllPath
Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\DataloaderCLI.exe $FolderFUllPath
Copy-Item DataLoaderCLI\DataloaderCLI\conf.ini $FolderFUllPath
