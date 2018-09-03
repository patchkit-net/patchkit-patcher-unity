#!/usr/bin/env sh

EXEDIR="."
PATCHER_EXE="Patcher"
LOCKFILE=""
NETWORK_STATUS="online"

while [ "$1" != "" ]; do
    PARAM=`echo $1 | awk -F= '{print $1}'`
    VALUE=`echo $1 | awk -F= '{print $2}'`
    case $PARAM in
        --exedir)
            EXEDIR=$VALUE
            ;;
        --patcher-exe)
            PATCHER_EXE=$VALUE
            ;;
        --secret)
            SECRET=$VALUE
            ;;
        --installdir)
            INSTALLDIR=$VALUE
            ;;
        --lockfile)
            LOCKFILE=$VALUE
            ;;
        --network-status)
            NETWORK_STATUS=$VALUE
            ;;
    esac
    shift
done

LD_DIRS="`find "$EXEDIR" -name "x86_64" -printf "%p:"`"
LD_DIRS="$LD_DIRS`find "$EXEDIR" -name "x86" -printf "%p:"`"

export LD_LIBRARY_PATH="$LD_DIRS"

ARGS="--installdir $INSTALLDIR --secret $SECRET"

if [ -n "$LOCKFILE" ]
then
    ARGS="$ARGS --lockfile $LOCKFILE"
fi

if [ ! -z "$NETWORK_STATUS" ]
then
    ARGS="$ARGS --${NETWORK_STATUS}"
fi

"$EXEDIR/$PATCHER_EXE" $ARGS