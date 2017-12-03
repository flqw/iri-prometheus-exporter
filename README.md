# IRI Prometheus Exporter

A prometheus proxy for the IRI API Commands ``getNodeInfo`` and ``getNeighbors`` for continuous monitoring of an IRI Node.

### Environment Variables

``IRI_API_URI`` The IRI Node API URI (e.g. http://localhost:14265)

### How does it work?

Provides a ``/metrics`` endpoint on container port 5000 that makes two POST calls (``{"command": "getNodeInfo"}``/
``{"command": "getNeighbors"}``) to ``IRI_API_URI`` 
and aggregates the information as prometheus metrics.

#### Docker Example
```
docker run -e IRI_API_URI=http://localhost:14265 -it -p 5000:5000 flqw/iri-prometheus-exporter:latest
```
