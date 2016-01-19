#create folder called DataLoaderCLI_%VERSION%
$NewFolderName = $args[0]

$FolderFUllPath = '\\sec-it-artifacts\Software\'+$NewFolderName

#New-Item 
New-Item $FolderFUllPath -ItemType Directory


#Copy from: \DataloaderCLI\DataloaderCLI\bin\Release
		#Aih.Dataloader.dll
		#DataloaderCLI

Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\Aih.Dataloader.dll $FolderFUllPath
Copy-Item DataLoaderCLI\DataloaderCLI\bin\Release\DataloaderCLI.exe $FolderFUllPath
# 

#Copy from DataloaderCLI\DataloaderCLI
		#conf.ini
Copy-Item DataLoaderCLI\DataloaderCLI\conf.ini


#Move folder to: \\sec-it-artifacts\Software



#Delete folder created at the begining