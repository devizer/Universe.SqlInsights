app=/transient-builds/s4app
mkdir -p $app
if [[ ! -s $app/ok ]]; then
  try-and-retry curl -kfSL -o /transient-builds/app.tar.xz https://github.com/devizer/Universe.SqlInsights/releases/latest/download/sqlinsights-dashboard-linux-x64.tar.xz
  tar xJf /transient-builds/app.tar.xz -C $app/
  echo ok > $app/ok
fi
$app/Universe.SqlInsights.W3Api --version
cat <<'EOF' > /transient-builds/s4app/launch-and-test.sh
Say "Launching S4Dashboard"
cat /proc/cpuinfo; 
free -m; 
./Universe.SqlInsights.W3Api &
sleep 40
try-and-retry curl -I http://localhost:40080/swagger
EOF
cat /transient-builds/s4app/launch-and-test.sh
chmod +x /transient-builds/s4app/launch-and-test.sh

container=s4-vm-container
docker rm -f $container
sqlServer=192.168.157.1
sqlServer=192.168.2.42
image=devizervlad/crossplatform-pipeline:x64-debian-12
try-and-retry try-and-retry try-and-retry try-and-retry docker pull $image
docker rm $container
docker run --privileged -p 22022:22022 -p 40080:40080 -e VM_MEM=640M -e VM_FORWARD_PORTS="hostfwd=tcp::40080-:40080" -e ASPNETCORE_URLS="http://*:40080" -e VM_VARIABLES='VM_FORWARD_PORTS;ASPNETCORE_URLS;ConnectionStrings__SqlInsights' -e ConnectionStrings__SqlInsights="Data Source=$sqlServer; Initial Catalog=SqlInsights Local Warehouse; User ID=sa; password=\`1qazxsw2;TrustServerCertificate=True" -e QEMU_TCG_ACCELERATOR=tcg -v /transient-builds/s4app:/job --name $container --hostname $container --device /dev/fuse --cap-add SYS_ADMIN --security-opt apparmor:unconfined -it $image bash -e -c "./Universe.SqlInsights.W3Api"
 

