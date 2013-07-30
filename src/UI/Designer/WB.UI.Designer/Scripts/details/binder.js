﻿define('binder',
    ['jquery', 'ko', 'config', 'vm'],
    function ($, ko, config, vm) {

        var ids = config.viewIds,
            bind = function () {
                $(document).ready(function () {
                    ko.applyBindings(vm.questionnaire);
                    $('input, textarea').placeholder();
                });
            };
        return {
            bind: bind
        };
    });