angular.module('dapp', [])
    .controller('MyCtrl', function ($scope, $timeout, $window) {
        function getAelf(){
            var aelf = null;
            if ($window.NightElf) {
                var appName = settings.contractInfo.contractName;
                aelf = new window.NightElf.AElf({
                    httpProvider: settings.rpcUrl, appName
                });
            } else {
                aelf = new AElf(new AElf.providers.HttpProvider(settings.rpcUrl));
            }
            return aelf;
        }
        function start(){
            $scope.aelf = getAelf();
            $scope.aelf.chain.getChainInformation((error, result) => {
                if (result) {
                    var dummyAccount = AElf.wallet.getWalletByPrivateKey('27e8be33c76669d5962a2b92b4651790f57ce9adfbfc7ec90bd98e990cc12b05'); // placeholder
                    var account = $scope.userAccount.address ? $scope.userAccount : dummyAccount;
                    setTimeout(() => {
                        $scope.aelf.chain.contractAtAsync(
                            settings.contractInfo.contractAddress,
                            account,
                            (e, r) => {
                                $scope.Ballot = r;
                            }
                        );
                    });
                }
            });
        }
        $scope.aelf = getAelf();
        $scope.userAccount = {};
        $scope.voter = {};
        $scope.proposals = [];
        $scope.txsByProposalId = {};
        $scope.voteFor = function (id) {
            var payload = {
                value: id
            };
            $scope.Ballot.Vote(payload, (error, result) => {
                if (result) {
                    $scope.txsByProposalId[id] = result.result.TransactionId;
                    // TODO: get transaction status and set voted
                }
            });
        }
        function hasBallot() {
            return $scope.Ballot && $scope.Ballot.GetProposals;
        }
        $scope.gettingVoter = false;
        function getVoter() {
            if ($scope.gettingVoter || !hasBallot() || !$scope.userAccount.address) {
                return;
            }
            $scope.voter.address = $scope.userAccount.address;
            $scope.gettingVoter = true;
            $scope.Ballot.GetVoter.call($scope.voter.address, (e, r) => {
                $scope.gettingVoter = false;
                if (r) {
                    Object.keys(r).forEach(function (key) {
                        $scope.voter[key] = r[key];
                    });
                }
            });
        }
        $scope.gettingProposals = false;
        function getProposals() {
            if ($scope.gettingProposals || !hasBallot()) {
                return;
            }
            $scope.gettingProposals = true;
            $scope.Ballot.GetProposals.call({}, (e, r) => {
                $scope.gettingProposals = false;
                if (r.proposals) {
                    $scope.proposals = r.proposals.slice(0);
                }
            });
        }
        $scope.hasNightElf = function(){
            return $window.NightElf;
        }

        $scope.login = function(){
            var appName = settings.contractInfo.contractName;
            $window.NightElf.api({
                appName: appName,
                method: 'LOGIN',
                chainId: 'AELF',
                payload: {
                    payload: {
                        method: 'LOGIN',
                        contracts: [settings.contractInfo]
                    }
                }
            }).then(result => {
                if (result.error == 0) {
                    $scope.userAccount = JSON.parse(result.detail);
                    start();
                }
            });
        }
        function getPermission(){
            $window.NightElf.api({
                appName: settings.contractInfo.contractName,
                method: 'CHECK_PERMISSION',
                chainId: 'AELF',
                payload: {
                    payload: {
                        method: 'CHECK_PERMISSION',
                        type: 'address',
                        address: settings.contractInfo.contractAddress
                    }
                }
            }).then(result => {
                if (result.error == 0) {
                    result.permissions.forEach(p =>{
                        if(p.contracts.some(c=>c.contractAddress == settings.contractInfo.contractAddress)){
                            $scope.userAccount = {address: p.address};
                            start();
                            $scope.$digest();
                        }
                    });
                }
            });
        }
        $window.document.addEventListener('NightElf', result => {getPermission();});

        function loop(callbacks) {
            $timeout(function () {
                loop(callbacks);
            }, 4000);
            callbacks.forEach(cb => cb());
        };

        start();
        loop([getVoter, getProposals]);
    });
