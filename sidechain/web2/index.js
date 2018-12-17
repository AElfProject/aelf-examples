var address = "";
var aelf = new Aelf(new Aelf.providers.HttpProvider(settings.rpcUrl));
aelf.isConnected();
var wallet = Aelf.wallet.getWalletByPrivateKey(settings.accounts[0].privkey);

var contract = aelf.chain.contractAt(settings.contractAddress, wallet);

function hex2a(hexx) {
    var hex = hexx.toString();//force conversion
    var str = '';
    for (var i = 0; (i < hex.length && hex.substr(i, 2) !== '00'); i += 2)
        str += String.fromCharCode(parseInt(hex.substr(i, 2), 16));
    return str;
}

angular.module('aelfdemo', [])
.controller('MyCtrl', function($scope, $timeout) {

    $scope.explorerUrl = settings.explorerUrl;

    function prepareContractInfo(){
        $scope.defaultwallet = Aelf.wallet.getWalletByPrivateKey(settings.accounts[0].privkey);
        $scope.defaultcontract = aelf.chain.contractAt(settings.contractAddress, $scope.defaultwallet);
        $scope.contractInfo = {};
        $scope.contractInfo.Symbol = hex2a($scope.defaultcontract.Symbol().return);
        $scope.contractInfo.TokenName = hex2a($scope.defaultcontract.TokenName().return);
        $scope.contractInfo.TotalSupply = parseInt("0x"+$scope.defaultcontract.TotalSupply().return);
    }

    $scope.accounts = settings.accounts;
    $scope.balances = settings.accounts.map(a=>0);
    $scope.chain = aelf.chain;
    $scope.updateBalances = null;
    $scope.updateBalances = function(){
        function getBalance(account){
            var wallet = Aelf.wallet.getWalletByPrivateKey(account.privkey);
            var contract = aelf.chain.contractAt(settings.contractAddress, wallet);
            var ret = contract.BalanceOf(account.address).return;
            return parseInt("0x"+ret);
        };
        $scope.accounts.forEach((account, i) => {
            $scope.accounts[i].bal = getBalance(account);
        });
        $timeout(function(){
            $scope.updateBalances();
        }, 4000);

    };
    prepareContractInfo();
    $scope.updateBalances();

    $scope.getTransactionResult = null;
    $scope.getTransactionResult = function(txnobj){
        var res = aelf.chain.getTxResult(txnobj.hash).result;
        txnobj.status = res.tx_status;
        if(res.tx_status == "Pending"){
            $timeout(function(){
                $scope.getTransactionResult(txnobj);
            }, 4000);
        }
    }

    $scope.sentTransactions = [];
    $scope.sendTransaction = function(){
        var wallet = Aelf.wallet.getWalletByPrivateKey($scope.accounts[$scope.fromIndex].privkey);
        var contract = aelf.chain.contractAt(settings.contractAddress, wallet);
        var hash = contract.Transfer($scope.accounts[$scope.toIndex].address, $scope.transferAmt).hash;
        var txnobj = {
            hash: hash,
            from: $scope.accounts[$scope.fromIndex].address,
            to: $scope.accounts[$scope.toIndex].address,
            amount: $scope.transferAmt
        };
        $scope.sentTransactions.push(txnobj);
        $scope.getTransactionResult(txnobj);
    };

    $scope.sendTransactionDep = function(){
        var wallet = Aelf.wallet.getWalletByPrivateKey($scope.accounts[$scope.fromIndexDep].privkey);
        var contract = aelf.chain.contractAt(settings.contractAddress, wallet);
        var hash = contract.DependentTransfer(
            $scope.accounts[$scope.toIndexDep].address,
            $scope.transferAmtDep,
            $scope.hashDep,
            $scope.merklePathDep,
            $scope.parentHeightDep
        ).hash;
        var txnobj = {
            hash: hash,
            from: $scope.accounts[$scope.fromIndexDep].address,
            to: $scope.accounts[$scope.toIndexDep].address,
            amount: $scope.transferAmtDep
        };
        $scope.sentTransactions.push(txnobj);
        $scope.getTransactionResult(txnobj);
    };
});