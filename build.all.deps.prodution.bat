start cmd /c "TITLE Designer Questionnaire && cd src\UI\Designer\WB.UI.Designer\questionnaire && yarn && yarn gulp --production"
call cmd  /c "TITLE hq deps && cd src\UI\Headquarters\WB.UI.Headquarters\Dependencies && yarn && yarn gulp --production"
start cmd /c "TITLE Hq App && cd src\UI\Headquarters\WB.UI.Headquarters\HqApp && yarn && yarn gulp --production"
