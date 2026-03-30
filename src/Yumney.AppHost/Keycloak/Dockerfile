FROM quay.io/keycloak/keycloak:26.5 AS builder

WORKDIR /opt/keycloak

ENV KC_DB=postgres
ENV KC_HTTP_ENABLED=true
ENV KC_HEALTH_ENABLED=true

RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:26.5

COPY --from=builder /opt/keycloak/ /opt/keycloak/
COPY Realms/ /opt/keycloak/data/import/
