#!/usr/bin/env bash

EXEDIR=$1
PATCHER_EXE=$2
SECRET=$3
INSTALLDIR=$4
LOCKFILE=$5

LD_DIRS=`find $EXEDIR -name "x86_64" -printf "%p:"`
LD_DIRS=$LD_DIRS`find $EXEDIR -name "x86" -printf "%p:"`

export LD_LIBRARY_PATH=$LD_DIRS

if [ -n $LOCKFILE ]
then
    $EXEDIR/$PATCHER_EXE --installdir $INSTALLDIR --secret $SECRET --lockfile $LOCKFILE
else
    $EXEDIR/$PATCHER_EXE --installdir $INSTALLDIR --secret $SECRET
fi