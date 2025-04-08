c:\x\nswag\win\nswag openapi2csclient /input:http://localhost:50420/swagger/v1/swagger.json ^
                       /classname:S5ApiClient ^
                       /namespace:Slices.Dashboard ^
                       /output:Slices.Dashboard.S5ApiClient.cs

@echo TODO: REPLACE 512 MAX ERROR LENGTH ON REGENERATE [Length ^>= 512 ? 512]
C:\Apps\Git\usr\bin\sed.exe -i -E "s|Length >= 512 \? 512|Length >= 512000 \? 512000|" Slices.Dashboard.S5ApiClient.cs 

