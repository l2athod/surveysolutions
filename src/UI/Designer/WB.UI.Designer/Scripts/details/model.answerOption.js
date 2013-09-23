﻿define('model.answerOption',
    ['ko','config'],
    function (ko, config) {
        var AnswerOption = function () {
            var self = this;
            
            self.id = ko.observable(Math.uuid());
            self.title = ko.observable().extend({ required: true });
            self.value = ko.observable().extend({ required: true, number: true }).trimmedNumber();
            
            self.image = ko.observable();
            
            self.isNullo = false;
            
            self.errors = ko.validation.group(self);
            
            return self;
        };

        AnswerOption.Nullo = new AnswerOption().id(0).title('');
        AnswerOption.Nullo.isNullo = true;

        return AnswerOption;
});
