﻿angular.module('designerApp')
    .controller('LeftMenuCtrl',
        function ($rootScope, $scope) {
            'use strict';

            $scope.isUnfoldedChapters = false;
            $scope.isUnfoldedScenarios = false;
            $scope.isUnfoldedMacros = false;
            $scope.isUnfoldedLookupTables = false;
            $scope.isUnfoldedAttachments = false;
            $scope.isUnfoldedTranslations = false;
            $scope.isUnfoldedMetadata = false;
            $scope.isUnfoldedComments = false;
            $scope.isUnfoldedCategories = false;


            var closeOpenPanelIfAny = function() {
                if (!($scope.isUnfoldedChapters || $scope.isUnfoldedScenarios || $scope.isUnfoldedMacros || $scope.isUnfoldedLookupTables || $scope.isUnfoldedAttachments 
                    || $scope.isUnfoldedTranslations || $scope.isUnfoldedMetadata  || $scope.isUnfoldedComments || $scope.isUnfoldedCategories))
                    return;

                if ($scope.isUnfoldedChapters) {
                    $rootScope.$broadcast("closeChaptersListRequested", {});
                }
                if ($scope.isUnfoldedScenarios) {
                    $rootScope.$broadcast("closeScenariosListRequested", {});
                }
                if ($scope.isUnfoldedMacros) {
                    $rootScope.$broadcast("closeMacrosListRequested", {});
                }
                if ($scope.isUnfoldedLookupTables) {
                    $rootScope.$broadcast("closeLookupTablesRequested", {});
                }
                if ($scope.isUnfoldedAttachments) {
                    $rootScope.$broadcast("closeAttachmentsRequested", {});
                }
                if ($scope.isUnfoldedTranslations) {
                    $rootScope.$broadcast("closeTranslationsRequested", {});
                }
                if ($scope.isUnfoldedMetadata) {
                    $rootScope.$broadcast("closeMetadataRequested", {});
                }
                if ($scope.isUnfoldedComments) {
                    $rootScope.$broadcast("closeCommentsRequested", {});
                }
                if ($scope.isUnfoldedCategories) {
                    $rootScope.$broadcast("closeCategoriesRequested", {});
                }

            };

            var closeAllPanel = function () {
                $scope.isUnfoldedScenarios = false;
                $scope.isUnfoldedMacros = false;
                $scope.isUnfoldedChapters = false;
                $scope.isUnfoldedLookupTables = false;
                $scope.isUnfoldedAttachments = false;
                $scope.isUnfoldedTranslations = false;
                $scope.isUnfoldedMetadata = false;
                $scope.isUnfoldedComments = false;
                $scope.isUnfoldedCategories = false;
            }

            $scope.unfoldChapters = function () {
                if ($scope.isUnfoldedChapters)
                    return;
                closeOpenPanelIfAny();
                $scope.isUnfoldedChapters = true;
                $rootScope.$broadcast("openChaptersList", {});
            };

            $scope.unfoldCategories = function () {
                if ($scope.isUnfoldedCategories)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedCategories = true;
                $rootScope.$broadcast("openCategories", {});
            };

            $scope.unfoldMacros = function () {
                if ($scope.isUnfoldedMacros)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedMacros = true;
                $rootScope.$broadcast("openMacrosList", {});
            };

            $scope.unfoldScenarios = function () {
                if ($scope.isUnfoldedScenarios)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedScenarios = true;
                $rootScope.$broadcast("openScenariosList", {});
            };

            $scope.unfoldLookupTables = function () {
                if ($scope.isUnfoldedLookupTables)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedLookupTables = true;
                $rootScope.$broadcast("openLookupTables", {});
            };

            $scope.unfoldAttachments = function () {
                if ($scope.isUnfoldedAttachments)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedAttachments = true;
                $rootScope.$broadcast("openAttachments", {});
            };

            $scope.unfoldTranslations = function () {
                if ($scope.isUnfoldedTranslations)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedTranslations = true;
                $rootScope.$broadcast("openTranslations", {});
            };

            $scope.unfoldMetadata = function () {
                if ($scope.isUnfoldedMetadata)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedMetadata = true;
                $rootScope.$broadcast("openMetadata", {});
            };

            $scope.unfoldComments = function () {
                if ($scope.isUnfoldedComments)
                    return;

                closeOpenPanelIfAny();
                $scope.isUnfoldedComments = true;
                $rootScope.$broadcast("openComments", {});
            };

            $scope.$on('openCategoriesList', function () {
                $scope.isUnfoldedCategories = true;
            });

            $scope.$on('openChaptersList', function () {
                $scope.isUnfoldedChapters = true;
            });

            $scope.$on('openLookupTables', function () {
                $scope.isUnfoldedLookupTables = true;
            });

            $scope.$on('openAttachments', function () {
                $scope.isUnfoldedAttachments = true;
            });

            $scope.$on('openTranslations', function () {
                $scope.isUnfoldedTranslations = true;
            });

            $scope.$on('openMetadata', function () {
                $scope.isUnfoldedMetadata = true;
            });

            $scope.$on('openComments', function () {
                $scope.isUnfoldedComments = true;
            });

            $scope.$on('openScenariosList', function () {
                $scope.isUnfoldedScenarios = true;
            });

            $scope.$on('openMacrosList', function () {
                $scope.isUnfoldedMacros = true;
            });

            $scope.$on('closeCategories', function () {
                closeAllPanel();
            });

            $scope.$on('closeChaptersList', function () {
                closeAllPanel();
            });

            $scope.$on('closeScenariosList', function () {
                closeAllPanel();
            });

            $scope.$on('closeMacrosList', function () {
                closeAllPanel();
            });

            $scope.$on('closeLookupTables', function () {
                closeAllPanel();
            });
            
            $scope.$on('closeAttachments', function () {
                closeAllPanel();
            });

            $scope.$on('closeTranslations', function () {
                closeAllPanel();
            });

            $scope.$on('closeMetadata', function () {
                closeAllPanel();
            });

            $scope.$on('closeComments', function () {
                closeAllPanel();
            });

            $scope.$on('verifing', function () {
                closeOpenPanelIfAny();
            });
        });
