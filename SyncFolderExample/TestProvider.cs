using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Synchronization;
using Microsoft.Synchronization.MetadataStorage;
using Microsoft.Synchronization.SimpleProviders;

namespace SyncFolderExample
{
    internal class TestProvider : FullEnumerationSimpleSyncProvider, ISimpleSyncProviderConcurrencyConflictResolver, ISimpleSyncProviderConstraintConflictResolver, IDisposable
    {
        private readonly string _metadataFilePath;
        // The version of the provider (not the version of Sync Framework).
        // Provider version can be used to ensure that two different versions of a provider
        // can work with each other. For example, if you release version 2 of a provider, but
        // no fundamtental changes were made to data and metadata storage, handling, or transfer,
        // the new provider could retain a provider version of 1, which means the two providers
        // can interoperate.
        const short PROVIDER_VERSION = 1;

        // The integer value for each column in the item store. These values are used when
        // creating custom field definitions and identity rules for the ItemMetadataSchema.
        public const uint CUSTOM_FIELD_ID = 1;
        public const uint CUSTOM_FIELD_TIMESTAMP = 2;

        private readonly SyncId _replicaId;

        private readonly SyncIdFormatGroup _idFormats;
        private readonly ItemMetadataSchema _metadataSchema;
        
        private volatile bool _disposed;

        private SqlMetadataStore _metadataStore;

        public override SyncIdFormatGroup IdFormats
        {
            get
            {
                return
                    _idFormats;
            }
        }

        public override ItemMetadataSchema MetadataSchema
        {
            get
            {
                return
                    _metadataSchema;
            }
        }

        public override short ProviderVersion
        {
            get
            {
                return
                    PROVIDER_VERSION;
            }
        }

        public TestProvider(
            Guid replicaId,
            string metadataFilePath
            )
        {
            _metadataFilePath = metadataFilePath;
            _replicaId = new SyncId(replicaId);

            //// Set ReplicaIdFormat to use a GUID as an ID, and ItemIdFormat to use a GUID plus
            //// an 8-byte prefix.
            //_idFormats = new SyncIdFormatGroup();
            //_idFormats.ItemIdFormat.IsVariableLength = false;
            //_idFormats.ItemIdFormat.Length = 24;
            //_idFormats.ReplicaIdFormat.IsVariableLength = false;
            //_idFormats.ReplicaIdFormat.Length = 16;

            this._idFormats = new SyncIdFormatGroup();
            this._idFormats.ChangeUnitIdFormat.IsVariableLength = false;
            this._idFormats.ChangeUnitIdFormat.Length = (ushort)4;
            this._idFormats.ItemIdFormat.IsVariableLength = false;
            this._idFormats.ItemIdFormat.Length = (ushort)24;
            this._idFormats.ReplicaIdFormat.IsVariableLength = false;
            this._idFormats.ReplicaIdFormat.Length = (ushort)16;

            CreateMetadataStore(metadataFilePath);
            _metadataSchema = CreateMetadataSchema();
        }

        public override void BeginSession()
        {
            //nothing to do
        }

        public override MetadataStore GetMetadataStore(out SyncId replicaId, out CultureInfo culture)
        {
            CreateMetadataStore(_metadataFilePath);

            replicaId = _replicaId;
            culture = CultureInfo.InvariantCulture;
            
            return
                _metadataStore;
        }

        public override object LoadChangeData(ItemFieldDictionary keyAndExpectedVersion, IEnumerable<SyncId> changeUnitsToLoad, RecoverableErrorReportingContext recoverableErrorReportingContext)
        {
            throw new NotImplementedException();
        }

        public override void InsertItem(object itemData, IEnumerable<SyncId> changeUnitsToCreate, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary keyAndUpdatedVersion, out bool commitKnowledgeAfterThisItem)
        {
            throw new NotImplementedException();
        }

        public override void UpdateItem(object itemData, IEnumerable<SyncId> changeUnitsToUpdate, ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary keyAndUpdatedVersion, out bool commitKnowledgeAfterThisItem)
        {
            throw new NotImplementedException();
        }

        public override void DeleteItem(ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out bool commitKnowledgeAfterThisItem)
        {
            throw new NotImplementedException();
        }

        public override void EndSession()
        {
            CloseMetadataStore();
        }

        public override void EnumerateItems(FullEnumerationContext context)
        {
            List<ItemFieldDictionary> items = new List<ItemFieldDictionary>();
            //foreach (ulong id in _metadataStore.Ids)
            //{
            //    items.Add(_store.CreateItemFieldDictionary(id));
            //}
            context.ReportItems(items);
        }

        public void ResolveUpdateUpdateConflict(object itemData, IEnumerable<SyncId> changeUnitsToMerge, IEnumerable<SyncId> changeUnitsToUpdate, ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary updatedVersion)
        {
            throw new NotImplementedException();
        }

        public void ResolveLocalDeleteRemoteUpdateConflict(object itemData, IEnumerable<SyncId> changeUnitsToUpdate, RecoverableErrorReportingContext recoverableErrorReportingContext, out bool itemWasDeletedAsResultOfResolution, out ItemFieldDictionary updatedVersion)
        {
            throw new NotImplementedException();
        }

        public void ResolveLocalUpdateRemoteDeleteConflict(ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out bool itemWasDeletedAsResultOfResolution)
        {
            throw new NotImplementedException();
        }

        public void MergeConstraintConflict(object itemData, ConflictVersionInformation conflictVersionInformation, IEnumerable<SyncId> changeUnitsToMerge, ItemFieldDictionary localConflictingItem, ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary updatedKeyAndVersion)
        {
            throw new NotImplementedException();
        }

        public void ModifyAndInsertRemoteItem(object itemData, IEnumerable<SyncId> changeUnitsToCreate, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary updatedKeyAndVersion)
        {
            throw new NotImplementedException();
        }

        public void ModifyAndUpdateRemoteItem(object itemData, IEnumerable<SyncId> changeUnitsToUpdate, ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary updatedKeyAndVersion)
        {
            throw new NotImplementedException();
        }

        public void ModifyLocalItem(ItemFieldDictionary keyAndExpectedVersion, RecoverableErrorReportingContext recoverableErrorReportingContext, out ItemFieldDictionary updatedKeyAndVersion)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                CloseMetadataStore();
            }
        }

        #region private code

        private void CreateMetadataStore(
            string metadataFilePath
            )
        {
            if (metadataFilePath == null)
            {
                throw new ArgumentNullException("metadataFilePath");
            }

            SqlMetadataStore result;

            // Create or open the metadata store, initializing it with the ID formats 
            // that are used to reference items and replicas.
            if (!File.Exists(metadataFilePath))
            {
                result = SqlMetadataStore.CreateStore(metadataFilePath);
            }
            else
            {
                result = SqlMetadataStore.OpenStore(metadataFilePath);
            }

            _metadataStore = result;
        }

        private void CloseMetadataStore()
        {
            _metadataStore.Dispose();
        }

        private ItemMetadataSchema CreateMetadataSchema()
        {
            var customFields = new CustomFieldDefinition[2];
            customFields[0] = new CustomFieldDefinition(CUSTOM_FIELD_ID, typeof (ulong));
            customFields[1] = new CustomFieldDefinition(CUSTOM_FIELD_TIMESTAMP, typeof (ulong));

            var identityRule = new IdentityRule[1];
            identityRule[0] = new IdentityRule(new uint[] {CUSTOM_FIELD_ID});

            var result = new ItemMetadataSchema(customFields, identityRule);

            return
                result;
        }

        #endregion

    }
}
