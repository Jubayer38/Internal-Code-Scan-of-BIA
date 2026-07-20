# =========================
# Base runtime image
# =========================
FROM mcr.microsoft.com/dotnet/aspnet@sha256:ebdd28e9ee54ea5032a390500d37bb1b6d45c36c6ba51e10f3ddfcdc746f3e28 AS base

WORKDIR /app
USER root
SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# Install packages and timezone
RUN ls -alht --color=auto /etc/apt/sources.list.d && \
    sed -i 's|http://deb.debian.org|https://deb.debian.org|g' /etc/apt/sources.list.d/debian.sources && \
    sed -i 's|http://security.debian.org|https://security.debian.org|g' /etc/apt/sources.list.d/debian.sources && \
    apt-get update && \
    apt-get install -y --no-install-recommends apt-transport-https ca-certificates less tzdata && \
    ln -snf /usr/share/zoneinfo/Asia/Dhaka /etc/localtime && \
    echo "Asia/Dhaka" > /etc/timezone && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Directories
RUN mkdir -p /data /app/log/applicationLogs

# ---- user setup (robust UID 102 handling) ----
ARG APP_USER=appuser
ARG APP_GROUP=appuser
ARG APP_UID=102
ARG APP_GID=102
ENV APP_USER=${APP_USER} APP_GROUP=${APP_GROUP} APP_UID=${APP_UID} APP_GID=${APP_GID}

RUN set -eux; \
  echo "DEBUG: UID=${APP_UID} GID=${APP_GID}"; \
  if getent group "${APP_GID}" >/dev/null; then \
    echo "GID ${APP_GID} already exists"; \
  else \
    groupadd -g "${APP_GID}" "${APP_GROUP}"; \
  fi; \
  if getent passwd "${APP_UID}" >/dev/null; then \
    existing_user="$(getent passwd ${APP_UID} | cut -d: -f1)"; \
    [ "${existing_user}" = "${APP_USER}" ] || usermod -l "${APP_USER}" "${existing_user}"; \
    usermod -g "${APP_GID}" "${APP_USER}" || true; \
    usermod -d "/home/${APP_USER}" -m "${APP_USER}" || true; \
  else \
    useradd -u "${APP_UID}" -g "${APP_GID}" -m -s /bin/bash "${APP_USER}"; \
  fi

# Aliases
RUN echo "alias ls='ls -alht --color=auto'" >> /etc/bash.bashrc && \
    echo "alias ls='ls -alht --color=auto'" >> /home/${APP_USER}/.bashrc

# Permissions
RUN chown -R ${APP_USER}:${APP_GID} /app /data && chmod -R 775 /app /data

# Entrypoint
RUN echo '#!/bin/bash' > /usr/local/bin/startApp.sh && \
    echo "echo \"Starting .NET application as ${APP_USER}: BIA.dll\"" >> /usr/local/bin/startApp.sh && \
    echo "exec dotnet BIA.dll" >> /usr/local/bin/startApp.sh && \
    chmod +x /usr/local/bin/startApp.sh

USER ${APP_USER}
EXPOSE 8080 8081

# =========================
# Build & publish stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk@sha256:df1aebc5fd72a1315f34eda24206f195d5ca00ccf2e3009947a74c5a67166cbb AS build
ENV PROJECT_NAME=BIA BUILD_CONFIGURATION=Release
WORKDIR /src

# --- Copy only project files first (to keep restore cache valid) ---
# Copy the SharedProjectFile.proj to the root level first
COPY ["SharedProjectFile.proj", "./"]
COPY ["${PROJECT_NAME}/${PROJECT_NAME}.csproj", "${PROJECT_NAME}/"]
COPY ["BIA.BLL/BIA.BLL.csproj", "BIA.BLL/"]
COPY ["BIA.DAL/BIA.DAL.csproj", "BIA.DAL/"]
COPY ["BIA.Entity/BIA.Entity.csproj", "BIA.Entity/"]

# --- Use BuildKit cache for NuGet packages ---
RUN --mount=type=cache,id=nuget-bia,target=/root/.nuget/packages \
    dotnet restore "${PROJECT_NAME}/${PROJECT_NAME}.csproj"

# --- copy full source and build ---
# Copy all necessary source code directories
COPY ["${PROJECT_NAME}/", "${PROJECT_NAME}/"]
COPY ["BIA.BLL/", "BIA.BLL/"]
COPY ["BIA.DAL/", "BIA.DAL/"]
COPY ["BIA.Entity/", "BIA.Entity/"]

WORKDIR "/src/${PROJECT_NAME}"
RUN --mount=type=cache,id=nuget-bia,target=/root/.nuget/packages \
    dotnet build "${PROJECT_NAME}.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish

FROM build AS publish
RUN --mount=type=cache,id=nuget-bia,target=/root/.nuget/packages \
    dotnet publish "${PROJECT_NAME}.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish -r linux-x64 --self-contained false
# =========================
# Final runtime image
# =========================

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["/usr/local/bin/startApp.sh"]
