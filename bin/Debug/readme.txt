- this is just a demo app provided "as is"
- it has no way to exit, taskkill -im autotran.exe or kill from task manager
- translates using azure cognitive services, add your API key in AutoTran.exe.config
Refer to https://azure.microsoft.com/en-us/services/cognitive-services/

Place key in .config file:

     <setting name="CognitiveServicesApiKey" serializeAs="String">
                <value>PUT KEY HERE</value>
            </setting>

