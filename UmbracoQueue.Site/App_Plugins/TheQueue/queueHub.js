(function () {
    'use strict';

    function queueHub($rootScope, $q, assetsService) {

        var starting = false;
        var callbacks = [];

        var scripts = [
            Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/lib/signalr/jquery.signalR.js',
            Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/backoffice/signalr/hubs'];

        return {
            initHub: initHub,
            clientId: clientId
        };

        //////////////

        function clientId() {
            if ($.connection !== undefined && $.connection.hub !== undefined) {
                return connection.hub.id;
            }
            return "";
        }

        function initHub(callback) {

            callbacks.push(callback);

            if (!starting) {
                if ($.connection === undefined) {
                    starting = true;

                    var promises = [];
                    scripts.forEach(function (script) {
                        promises.push(assetsService.loadJs(script));
                    });

                    $q.all(promises).then(function () {
                        setupCallbacks();
                    });
                }
                else {
                    setupCallbacks();
                }
            }
        }

        function setupCallbacks() {
            while (callbacks.length) {
                var cb = callbacks.pop();
                hubSetup(cb);
            }
            starting = false;
        }

        function hubSetup(callback) {
            var proxy = $.connection.queueHub;

            var hub = {
                start: function () {
                    $.connection.hub.start();
                },
                on: function (eventName, callback) {
                    proxy.on(eventName, function (result) { trigger(callback, result) });
                },
                invoke: function (methodName, callback) {
                    proxy.invoke(methodName)
                        .done(function (result) { trigger(callback, result) });
                }
            }
            return callback(hub);
        }

        function trigger(callback, result) {
            $rootScope.$apply(function () {
                if (callback) {
                    callback(result);
                }
            });
        }
    }

    angular.module('umbraco.resources')
        .factory('queueHub', queueHub);

})();