(function () {
    'use strict';

    function sendToQueueController($scope, navigationService,
        queueHub,
        theQueueService) {

        var vm = this;
        vm.sent = false; 
        vm.close = close;
        vm.queue = queue;
        vm.node = $scope.currentNode;

        vm.includeChildren = false;
        vm.includeUnpublished = false;

        vm.buttonState = 'init';

        initHub();

        ///////////////

        function close() {
            navigationService.hideDialog();
        }

        function queue() {
            vm.buttonState = 'busy';
            theQueueService.queueForPublish(vm.node.id, vm.includeChildren, vm.includeUnpublished)
                .then(function (result) {
                    vm.count = result.data;
                    vm.buttonState = 'success';
                    vm.sent = true;
                }, function (error) {
                    vm.buttonState = 'error';
                });
        }


        //////////

        vm.hub = null;
        vm.status = {};

        function initHub() {

            queueHub.initHub(function (hub) {

                vm.hub = hub;
                vm.hub.on('add', function (data) {
                    vm.status = data;
                });

                vm.hub.start();
            })
        }

        vm.calcPercentage = calcPercentage;

        function calcPercentage(status) {
            if (status !== undefined) {
                return (100 * status.count) / status.total;
            }
            return 1;
        }
    }

    angular.module('umbraco')
        .controller('sendToQueueController', sendToQueueController);
})();