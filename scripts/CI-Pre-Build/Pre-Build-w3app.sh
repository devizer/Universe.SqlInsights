         export NODE_VER=v16.20.2 SKIP_NPM_UPGRADE=True CI=False
         time (script=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-and-nodejs.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash -s node)
         cd src/universe.sqlinsights.w3app
         node --version
         time try-and-retry yarn install
         time yarn build
