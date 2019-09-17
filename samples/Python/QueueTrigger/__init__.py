import logging

import azure.functions as func


def main(order) -> str:
    logging.info('Python queue trigger function processed a queue item: %s',
                 order)
