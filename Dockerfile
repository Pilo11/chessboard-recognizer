FROM python:3.7
ADD . .
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb \
  && apt-get update && apt-get install -y dotnet-sdk-3.1 bash virtualenv \
  && git clone https://github.com/official-stockfish/Stockfish stockfish \
  && cd stockfish/src \
  && make build ARCH=x86-64-modern \
  && make install
RUN ["/bin/bash", "-c", "virtualenv venv && source venv/bin/activate && pip3 install -r requirements.txt"]
RUN ["python", "./generate_tiles.py"]
RUN ["python", "./train.py"]
EXPOSE 5000
ENTRYPOINT ["dotnet", "run", "--project", "ChessService"]
