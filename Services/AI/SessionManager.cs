using Tensorflow;
using System;

namespace IReadThis.Recommender.Services.AI
{
    /// <summary>
    /// Gerencia o ciclo de vida e o acesso thread-safe à Session única do TensorFlow.
    /// Garante que a Session não seja descartada prematuramente e que operações simultâneas
    /// sejam sincronizadas adequadamente.
    /// </summary>
    public class SessionManager : IDisposable
    {
        private Session _session;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public SessionManager(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session), "Session cannot be null.");

            _session = session;
        }

        /// <summary>
        /// Retorna a Session de forma thread-safe, verificando se foi descartada.
        /// </summary>
        public Session GetSession()
        {
            lock (_lockObject)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SessionManager), "Session has been disposed.");

                if (_session == null)
                    throw new InvalidOperationException("Session is null.");

                return _session;
            }
        }

        /// <summary>
        /// Verifica se a Session está em estado válido.
        /// </summary>
        public bool IsValid()
        {
            lock (_lockObject)
            {
                return !_disposed && _session != null;
            }
        }

        /// <summary>
        /// Descarta a Session e seus recursos associados.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                if (!_disposed)
                {
                    try
                    {
                        _session?.Dispose();
                        _session = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao descartar Session: {ex.Message}");
                    }
                    finally
                    {
                        _disposed = true;
                    }
                }
            }
        }
    }
}
