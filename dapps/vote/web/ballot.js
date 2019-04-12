/**
 * @file tokenDemo.js
 * @author zhouminghui
 * @description How to use the plugin for token transfer
*/
(function(){
    const address = '4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6';
    const rpcUrl = "http://127.0.0.1:1234/chain"
    const appName = 'Ballot';
    
    const contractInfo = {
        chainId: 'AELF',
        contractAddress: address,
        contractName: 'Ballot',
        description: 'A contract to vote for proposals.',
        github: ''
    }
    
    window.Ballot = {};
    
    document.addEventListener('NightElf', result => {
        window.NightElf.api({
            appName,
            method: 'LOGIN',
            chainId: 'AELF',
            payload: {
                payload: {
                    method: 'LOGIN',
                    contracts: [contractInfo]
                }
            }
        }).then(result => {
            if (result.error == 0) {
                window.userAccount = JSON.parse(result.detail);
                aelf.chain.getChainInformation((error, result) => {
                    if (result) {
                        setTimeout(() => {
                            aelf.chain.contractAtAsync(
                                address,
                                window.userAccount,
                                (e, r) => {
                                    window.Ballot = r;
                                }
                            );
                        });
                    }
                });
                
            }
        });
    
        const aelf = new window.NightElf.AElf({
            httpProvider: rpcUrl,
            appName
        });
    });
})();
