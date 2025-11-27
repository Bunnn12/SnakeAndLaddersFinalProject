namespace SnakeAndLaddersFinalProject.Policies
{
    internal static class LobbyMessages
    {
        public const int LOBBY_ID_NOT_SET = 0;
        public const int INVALID_USER_ID = 0;

        public const string STATUS_LOBBY_READY = "Lobby listo.";
        public const string STATUS_NO_LOBBY = "Sin lobby";
        public const string STATUS_LOBBY_CLOSED = "El lobby se cerró o expiró.";
        public const string STATUS_CODE_COPIED = "Código copiado al portapapeles.";
        public const string STATUS_JOIN_FAILED_PREFIX = "No se pudo entrar: ";
        public const string STATUS_CREATE_ERROR_PREFIX = "Error creando lobby: ";
        public const string STATUS_JOIN_ERROR_PREFIX = "Error al unirse: ";
        public const string STATUS_START_ERROR_PREFIX = "Error al iniciar: ";
        public const string STATUS_LEAVE_ERROR_PREFIX = "Error al salir: ";
        public const string STATUS_LEAVE_DEFAULT = "Saliste del lobby.";
        public const string STATUS_NO_VALID_PLAYERS = "No hay jugadores válidos para crear el tablero.";

        public const string LOBBY_STATUS_WAITING = "Waiting";
        public const string LOBBY_STATUS_IN_MATCH = "InMatch";
    }
}
