(function(exports){
    exports.rpcUrl = "http://path/to/sidechain-2/chain";
    // exports.rpcUrl = "http://localhost:12342/chain";
    exports.explorerUrl = "https://explorer.sidechain-1";
    exports.contractAddress = "ELF_JjysPHXYe2FqwZJBXok4WFwWbzpN7ZKNCuM9XtttMpK5LHuYP"; // Update after deploying contract
    exports.accounts = [
        {
            privkey: "13cd6aab18b99fc5842492c8e1e51a6473bd6034954698cc1cdc8e6f2add15c8",
            address: "ELF_aaHQLivdEfea51fEDft6q2iJgxUPqFT8W1p3nw3kXn8nDG1t"
        },
        {
            privkey: "64af7a444f58e566a8b007a5a191c0b89b13becec7de4171e6cbe9634ec7dbf0",
            address: "ELF_bbdAr12rJPKV2fA3LNZXqxa7pp9upaejNJZVKVejG2ZUKykCr"
        },
        {
            privkey: "e3a9bc19bdecd03881740de741d889582d58463b611ed0fbb92b81e6b57ccf20",
            address: "ELF_ccgwfSiaoF1qEqWtSWtNWkG7HJ2DiFHhanQZGeZGrGSZubWEE"
        }];
    // exports.privkey = "13cd6aab18b99fc5842492c8e1e51a6473bd6034954698cc1cdc8e6f2add15c8";
  }(typeof exports === 'undefined' ? this.settings = {} : exports));