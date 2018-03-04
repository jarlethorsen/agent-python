import sys
import logging
from types import MappingProxyType
import struct

from htpclient.dicts import copyAndSetToken, dict_clientError
from htpclient.jsonRequest import JsonRequest


def logErrorAndExit(message):
    logging.error(message)
    sys.exit(1)


def printSpeed(speed):
    prefixes = MappingProxyType(
        {0: "",
         1: "k",
         2: "M",
         3: "G",
         4: "T"})
    exponent = 0
    while speed > 1000:
        exponent += 1
        speed = float(speed) / 1000
    return str("{:6.2f}".format(speed)) + prefixes[exponent] + "H/s"


def get_bit():
    if struct.calcsize('P') * 8 == 64:
        return "64"
    return "32"


def send_error(error, token, task_id):
    query = copyAndSetToken(dict_clientError, token)
    query['message'] = error
    query['taskId'] = task_id
    JsonRequest(query)
