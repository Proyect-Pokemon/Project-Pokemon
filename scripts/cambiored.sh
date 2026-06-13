#!/usr/bin/bash

set -e

PREFIX=$1

if [ -z "$PREFIX" ]; then
  echo "Uso: ./cambio-red.sh 192.168.1"
  exit 1
fi

FILE="/etc/network/interfaces"

echo "Cambiando vmbr0 a $PREFIX.134"

cp $FILE ${FILE}.bak

# Cambiar IP
sed -i "s/address .*/address $PREFIX.134\/24/" $FILE

# Cambiar gateway
sed -i "s/gateway .*/gateway $PREFIX.1/" $FILE

echo "Reiniciando red..."
systemctl restart networking

echo "OK -> Nueva IP: $PREFIX.134"
