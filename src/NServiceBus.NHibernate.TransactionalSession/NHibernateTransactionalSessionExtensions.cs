namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Features;
    using global::NHibernate;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class NHibernateTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<NHibernatePersistence> EnableTransactionalSession(
            this PersistenceExtensions<NHibernatePersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault<NHibernateTransactionalSession>();

            return persistenceExtensions;
        }

        /// <summary>
        /// Opens the transactional session
        /// </summary>
        public static Task Open(this ITransactionalSession transactionalSession, CancellationToken cancellationToken = default)
            => transactionalSession.Open(new NHibernateOpenSessionOptions(), cancellationToken);

        /// <summary>
        /// Opens the transactional session
        /// </summary>
        public static ISession InitManualSessionMode(this ITransactionalSession transactionalSession, IServiceProvider sp, CancellationToken cancellationToken = default)
        {
            var sf = sp.GetService(typeof(ISessionFactory)) as ISessionFactory;
            var session = sf.OpenSession();
            transactionalSession.InitManualSessioMode(session, new NHibernateOpenSessionOptions(), cancellationToken).ConfigureAwait(false);

            return session;
        }
    }
}