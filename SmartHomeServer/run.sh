#!/bin/bash
cd /home/alessandro-atilano/Documentos/PR/Unity/SmartHomeServer/bin
java -cp ".:../lib/bson-4.11.1.jar:../lib/mongodb-driver-core-4.11.1.jar:../lib/mongodb-driver-sync-4.11.1.jar:../lib/jjwt-api-0.11.5.jar:../lib/jjwt-impl-0.11.5.jar:../lib/jjwt-jackson-0.11.5.jar:../lib/jackson-core-2.15.2.jar:../lib/jackson-databind-2.15.2.jar:../lib/jackson-annotations-2.15.2.jar" com.smarthome.server.TcpServer
