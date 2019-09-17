import logging
import json
import azure.functions as func


def write_http_response(status, body_dict):
    return_dict = {
        "status": status,
        "body": json.dumps(body_dict),
        "headers": {
            "Content-Type": "application/json"
        }
    }
    #return json.dumps(return_dict)
    return func.HttpResponse(
           json.dumps(return_dict),
           status_code=status
       )


def main(req: func.HttpRequest, RabbitMQ : func.Out[bytearray]):
    logging.info('Python HTTP trigger function processed a request.')

    if not req.method == 'POST':
        return func.HttpResponse(
            json.dumps({'message': 'Only post method is allowed.'}),
            status_code=400
        )

    req_body_str = json.dumps(req.get_json())
    # put on rabbitMQ queu
    RabbitMQ.set(req_body_str.encode())
    return func.HttpResponse(
        json.dumps({'message': 'Your request accepted.'}),
        status_code=202
    )
