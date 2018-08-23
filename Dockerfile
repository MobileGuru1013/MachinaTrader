FROM microsoft/dotnet:2.1-sdk-alpine AS restore
WORKDIR /app
COPY ./*.sln .
COPY ./*/*.csproj ./  
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore

FROM node as client-build
WORKDIR /app

COPY ./MachinaTrader/package.json .
RUN npm install
RUN npm install node-sass@latest

COPY ./MachinaTrader/build ./build
COPY ./MachinaTrader/wwwroot ./wwwroot
COPY ./MachinaTrader/.babelrc.js ./
COPY ./MachinaTrader/.eslintignore ./
COPY ./MachinaTrader/.eslintrc.json ./
RUN npm run build
RUN npm run build-vendors
RUN npm run css-compile

FROM restore as publish
WORKDIR /app/
COPY . .
COPY --from=client-build /app/wwwroot ./wwwroot
RUN dotnet publish MachinaTrader/MachinaTrader.csproj -o /app/out -c Release

FROM microsoft/dotnet:2.1-aspnetcore-runtime-alpine AS runtime
WORKDIR /app
COPY --from=publish /app/out .
ENTRYPOINT ["dotnet", "MachinaTrader.dll"]