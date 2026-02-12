         export NODE_VER=v16.20.2 SKIP_NPM_UPGRADE=True CI=False
         Say "BUILDING w3app usign NODE $NODE_VER"
         Run-Remote-Script https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-and-nodejs.sh node
         cd src/universe.sqlinsights.w3app
         node --version
         time try-and-retry yarn install
         time yarn build
