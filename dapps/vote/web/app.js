angular.module('dapp', [])
    .controller('MyCtrl', function ($scope, $timeout, $window) {
        $scope.voter = {};
        $scope.proposals = [];
        $scope.txsByProposalId = {};
        $scope.voteFor = function (id) {
            var payload = {
                value: id
            };

            $window.Ballot.Vote(payload, (error, result) => {
                if (result) {
                    $scope.txsByProposalId[id] = result.result.TransactionId;
                    // TODO: get transaction status and set voted
                }
            });
        }
        function hasBallot() {
            return $window.Ballot && $window.Ballot.GetProposals;
        }
        $scope.gettingVoter = false;
        function getVoter() {
            if ($scope.gettingVoter || !hasBallot()) {
                return;
            }
            $scope.voter.address = $window.userAccount.address;
            $scope.gettingVoter = true;
            $window.Ballot.GetVoter.call($scope.voter.address, (e, r) => {
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
            $window.Ballot.GetProposals.call({}, (e, r) => {
                $scope.gettingProposals = false;
                if (r.proposals) {
                    $scope.proposals = r.proposals.slice(0);
                }
            });
        }
        function loop(callbacks) {
            $timeout(function () {
                loop(callbacks);
            }, 4000);
            callbacks.forEach(cb => cb());
        };
        loop([getVoter, getProposals]);
    });
