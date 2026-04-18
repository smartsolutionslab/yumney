FROM quay.io/keycloak/keycloak:26.5 AS builder
COPY Realms/ /opt/keycloak/data/import/
ENV KC_DB=postgres
RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:26.5
COPY --from=builder /opt/keycloak/ /opt/keycloak/
COPY Realms/ /opt/keycloak/data/import/
ENV KC_DB=postgres
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
