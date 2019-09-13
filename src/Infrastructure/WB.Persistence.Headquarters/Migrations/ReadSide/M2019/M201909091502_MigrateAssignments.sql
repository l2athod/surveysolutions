﻿INSERT INTO readside.assignments (publickey, id, responsibleid, quantity, archived, createdatutc, updatedatutc, questionnaireid, questionnaireversion, answers, questionnaire, protectedvariables, receivedbytabletatutc, isaudiorecordingenabled, email, password, webmode)
SELECT 
    uuid_in(md5(random()::text || clock_timestamp()::text)::cstring),
	id,
	responsibleid, 
	quantity, 
	archived, 
	createdatutc, 
	updatedatutc, 
	questionnaireid, 
	questionnaireversion, 
	answers, 
	questionnaire, 
	protectedvariables, 
	receivedbytabletatutc, 
	isaudiorecordingenabled, 
	email, 
	password, 
	webmode
FROM plainstore.assignments;
	
INSERT INTO readside.assignmentsidentifyinganswers(assignmentid, "position", questionid, answer, answerasstring, rostervector)
SELECT 
	(SELECT ass.publickey FROM readside.assignments ass WHERE ass.id = ans.assignmentid), --assignmentid, 
	ans."position", 
	ans.questionid, 
	ans.answer, 
	ans.answerasstring, 
	ans.rostervector
FROM plainstore.assignmentsidentifyinganswers ans;
	
INSERT INTO events.events (id, origin, "timestamp", eventsourceid, eventsequence, eventtype, value)
SELECT 
	uuid_in(md5(random()::text || clock_timestamp()::text)::cstring),      -- id
	null,                -- origin
	createdatutc,        -- "timestamp"
	publickey,           -- eventsourceid
	1,                   -- eventsequence
	'AssignmentCreated', -- eventtype
	concat('{ 
		   "id": ', id, ', 
		   "userId": "00000000-0000-0000-0000-000000000001", 
		   "answers": ', answers::text, ', 
		   "webMode": ', webmode::text, ',
		   "quantity": ', quantity, ',
		   "originDate": "', createdatutc, '",
		   "responsibleId": "', responsibleId, '",
		   "protectedVariables": ', protectedVariables::text, ',
		   "questionnaireId": "', questionnaireid, '",
		   "questionnaireVersion": ', questionnaireversion, ',
		   "isAudioRecordingEnabled": ', isaudiorecordingenabled::text, '
	}')::jsonb          -- value
FROM readside.assignments;





