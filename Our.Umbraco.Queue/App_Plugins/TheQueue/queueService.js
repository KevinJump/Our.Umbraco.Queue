(function () {

    'use strict';

    function queueService($http) {

        var serviceRoot = Umbraco.Sys.ServerVariables.theQueue.queueService;

        return {
            getStatus: getStatus,
            queueForPublish: queueForPublish,
            getItems: getItems,
            process: process
        };

        /////////////////

        function getStatus() {
            return $http.get(serviceRoot + "GetStatus");
        }

        function queueForPublish(id, includeChildren, includeUnpublished) {
            return $http.post(serviceRoot + "QueueForPublish/" + id, {
                includeChildren: includeChildren,
                includeUnpublished: includeUnpublished
            });
        }

        function getItems(page) {
            return $http.get(serviceRoot + "GetItems?page=" + page);
        }

        function process() {
            return $http.post(serviceRoot + "ProcessQueue");
        }
    
    }

    angular.module('umbraco.services')
        .factory('theQueueService', queueService);

})();