# Import needed libraries
from esdbclient import NewEvent
import json
import config
import logging
import time
import traceback
import datetime
import random

import utils

log = logging.getLogger("creditcheck")

# Print some information messages to the user
log.info("***** CreditCheck *****")

if config.DEBUG:
    log.debug("Initializing...")
    log.debug('Connecting to ESDB...')

# Create a connection to ESDB
while True:
    try:
        esdb = utils.create_db_client()
        if config.DEBUG:
            log.debug('Connection succeeded!')
        
        # If successful, break from the while loop
        break
    # If the connection fails, print the exception and try again in 10 seconds
    except:
        log.error('Connection to ESDB failed, retrying in 10 seconds...')
        traceback.print_exc()
        time.sleep(10)

# Set up the name of the Event Type stream to which we will listen for events to check credit
_loan_request_stream_name=config.STREAM_ET_PREFIX+config.EVENT_TYPE_LOAN_REQUESTED

# Get a list of the current persistent subscriptions to ESDB with the given stream name
available_subscriptions = esdb.list_subscriptions_to_stream(stream_name=_loan_request_stream_name, )

# Check if the persistent subscription we want already exists
if len(available_subscriptions) == 0:
    if config.DEBUG:
        log.debug('Subscription doesn\'t exist, creating...')
    # If not, then create the persistent subscription
    esdb.create_subscription_to_stream(group_name=config.GROUP_NAME, stream_name=_loan_request_stream_name, resolve_links=True, message_timeout=600)
else:
    # if it does, then do nothing
    if config.DEBUG:
        log.debug('Subscription already exists. Skipping...')

# Start reading the persistent subscription
log.info('Waiting for LoanRequest events')
persistent_subscription = esdb.read_subscription_to_stream(group_name=config.GROUP_NAME, stream_name=_loan_request_stream_name, event_buffer_size=1)

# For each eent received
for loan_request_event in persistent_subscription:
    # Introduce some delay
    time.sleep(config.CLIENT_PRE_WORK_DELAY)

    log.info('Received event: id=' + str(loan_request_event.id) + '; type=' + loan_request_event.type + '; stream_name=' + str(loan_request_event.stream_name) + '; data=\'' +  str(loan_request_event.data) + '\'')

    # Get the current commit version of the loan request stream for this loan request
    COMMIT_VERSION = esdb.get_current_version(stream_name=loan_request_event.stream_name)

    # Get the event data out of JSON format and into a python dictionary so we can use it
    _loan_request_data = json.loads(loan_request_event.data)
    _loan_request_metadata = json.loads(loan_request_event.metadata)

    # Create a data structure to hold the credit score that we will return
    _credit_score = -1

    # Handle v1 events and convert User to Name
    try:
        _loan_request_data["Name"] = _loan_request_data.pop("User")
    except:
        time.sleep(0)

    # Map the requestor name to a credit score
    # In practice this portion of code would call out to some credit clearing house for an actual credit score, or apply other business rules
    if _loan_request_data.get("Name") == "Yves":
        _credit_score = 9
    elif _loan_request_data.get("Name") == "Tony":
        _credit_score = 5
    elif _loan_request_data.get("Name") == "David":
        _credit_score = 6
    elif _loan_request_data.get("Name") == "Rob":
        _credit_score = 1
    # If we can't map the  name to one of the above, generate a random score
    else:
        _credit_score = random.randint(1,10)

    _ts = str(datetime.datetime.now())

    # Create a dictionary with the credit check data
    _credit_checked_event_data = {"Score": _credit_score, "NationalID": _loan_request_data.get("NationalID"), "CreditCheckedTimestamp": _ts}
    _credit_checked_event_metadata = {"$correlationId": _loan_request_metadata.get("$correlationId"), "$causationId": str(loan_request_event.id), "transactionTimestamp": _ts}
    # Create a credit checked event with the event data
    credit_checked_event = NewEvent(type=config.EVENT_TYPE_CREDIT_CHECKED, metadata=bytes(json.dumps(_credit_checked_event_metadata), 'utf-8'), data=bytes(json.dumps(_credit_checked_event_data), 'utf-8'))
    
    if config.DEBUG:
        log.debug('Processing credit check - CreditChecked: ' + str(_credit_checked_event_data) + '\n')
    
    log.info('Processing credit check - CreditChecked for NationalID ' + str(_credit_checked_event_data["NationalID"]) + ' with a Score of ' + str(_credit_checked_event_data["Score"]))

    # Append the event to the stream
    CURRENT_POSITION = esdb.append_to_stream(stream_name=loan_request_event.stream_name, current_version=COMMIT_VERSION, events=[credit_checked_event])

    # Debug the ack_id
    if config.DEBUG:
        log.debug('Ack\'ing Persistent Subscription event with ack_id: ' + str(loan_request_event.ack_id))

    # Acknowledge the original event
    persistent_subscription.ack(loan_request_event.ack_id)

    # Introduce some delay
    time.sleep(config.CLIENT_POST_WORK_DELAY)

