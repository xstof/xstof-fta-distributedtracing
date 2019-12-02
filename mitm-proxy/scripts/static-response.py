from mitmproxy import http

def request(flow: http.HTTPFlow):
    # redirect to different host
    # if flow.request.pretty_host == "example.com":
    #     flow.request.host = "mitmproxy.org"
    # answer from proxy
    if flow.request.path.endswith("/pass"):
    	flow.response = http.HTTPResponse.make(
            200, b"I'm a teapot",
        )
    elif flow.request.path.endswith("/fail"):
    	flow.response = http.HTTPResponse.make(
            500, b"I'm not a teapot",
        )