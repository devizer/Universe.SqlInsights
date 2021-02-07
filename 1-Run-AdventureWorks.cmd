pushd src\
start /max "AdventureWorks" "C:\Program Files (x86)\IIS Express\iisexpress.exe"  /config:".\.vs\AdventureWorks\config\applicationhost.config" /site:"AdventureWorks" /apppool:"Clr4IntegratedAppPool"
start /max http://localhost:8776/
popd

