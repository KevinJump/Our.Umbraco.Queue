(function () {

    'use strict';

    function dashboardController(
        theQueueService, queueHub) {

        var vm = this;
        vm.status = {};
        vm.loading = true;
        vm.page = 1;
        vm.queue = { items: [] };

        vm.backgroundOn = Umbraco.Sys.ServerVariables.theQueue.backgroundOn; 

        vm.options = {
            includeProperties: [
                { alias: "action", header: "Action" },
                { alias: "priority", header: "Priority" },
                { alias: "submitted", header: "Submitted" },
                { alias: "attempt", header: "Retrys" }
            ]
        }

        vm.getItems = getItems;
        vm.process = process;

        vm.queueMax = 500;

        getStatus();

        function getStatus() {
            vm.loading = true; 
            theQueueService.getStatus()
                .then(function (result) {
                    vm.status = result.data;
                    if (vm.status.queueSize > 0) {
                        getItems(vm.page);
                    }
                    else {
                        vm.loading = false;
                    }
                    updateQueueMax();
                });
        }

        function getItems(page, loading = true) {
            vm.loading = loading;
            theQueueService.getItems(page)
                .then(function (result) {
                    vm.queue = result.data;
                    vm.loading = false;
                });
        }

        function process() {
            vm.status.processing = true;
            theQueueService.process()
                .then(function (result) {
                    console.log(result.data);
                    vm.status.processing = false;

                    getStatus();
                    refreshView();
                });
        }

        ////////////////

        function showNiceName(hashset, key) {
            if (hashset[key] !== undefined) {
                return hashset[key];
            }
            return key;
        }

        vm.knownActions = { 'contentPublish' : 'Publish' };
        vm.showAction = showAction;

        function showAction(action) {
            return showNiceName(vm.knownActions, action);
        }

        vm.priorities = { '0': 'Normal', '-100': 'Low', '100': 'High' };
        vm.showPriority = showPriority;

        function showPriority(priority) {
            return showNiceName(vm.priorities, priority);
        }

        ////////////////

        vm.nextPage = nextPage;
        vm.prevPage = prevPage;
        vm.goToPage = goToPage;

        function nextPage() {
            vm.page++;
            refreshView();
        }

        function prevPage() {
            vm.page--;
            refreshView();
        }

        function goToPage(pageNo) {
            vm.page = pageNo;
            refreshView();
        }

        function refreshView() {
            vm.getItems(vm.page);
        }

        function updateQueueMax() {
            if (vm.status.queueSize > vm.queueMax) {
                vm.queueMax = vm.status.queueSize;
            }
        }

        vm.calcQueuePercent = calcQueuePercent;
        vm.calcProgress = calcProgress;

        function calcQueuePercent() {
            if (vm.status !== undefined) {
                return calcPercentage(vm.status.queueSize, vm.queueMax);
            }
            return 1;
        }

        function calcProgress() {

            if (vm.status !== undefined) {
                return calcPercentage(vm.status.processed, vm.status.processed + vm.status.remaining);
            }
            return 1;
        }

        function calcPercentage(count, max) {
            return (100 * count) / max;
        }


        ///////////
        vm.hub = null;
        vm.status = {};


        function initHub() {

            queueHub.initHub(function (hub) {

                vm.hub = hub;
                vm.hub.on('progress', function (data) {
                    vm.status = data;

                    if (vm.status.refresh || vm.status.processed % 10 == 0) {
                        getItems(vm.page, false);
                    }

                    if (!vm.status.isProcessing) {
                        refreshView();
                    }

                    updateQueueMax();
                });

                vm.hub.start();
            })
        }

        initHub();

    }

    angular.module('umbraco')
        .controller('theQueueDashboardController', dashboardController);


})();