![Link Text](http://build.mysurvey.solutions/app/rest/builds/buildType:`(id:CI)`/statusIcon)
# 5.27
- After deployment of relase there is a script that should be executed from Gateway or Build server to sync images from disk to s3 storage - [deploy-tools/utils/upload-tos3.ps1](https://bitbucket.org/wbcapi/deploy-tools/src/master/utils/upload-tos3.ps1?at=master&fileviewer=file-view-default)
# 5.26
- .net framework 4.7.1 is required
# 5.19
- Interviewer app lower than `5.8` version won't be able to synchronize anymore
- HQ apps with version lower than `5.8` won't be able to import questionnaires from designer
- Main pg connection string now requires additional argument: providerName="Npgsql"
# 5.18
IE issue: GPS question cannot be answered 2 times in 1 minute http://issues.mysurvey.solutions/youtrack/issue/KP-8547
# 5.17
- Web sockets windows feature should be enabled
