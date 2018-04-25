// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </auto-generated>

namespace Microsoft.Media.Encoding.Rest.ArmClient.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Describes an Output from the Transform
    /// </summary>
    public partial class TransformOutput
    {
        /// <summary>
        /// Initializes a new instance of the TransformOutput class.
        /// </summary>
        public TransformOutput()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the TransformOutput class.
        /// </summary>
        /// <param name="preset">Preset that describes the Media Processor
        /// operation that will be used to generate the output.</param>
        /// <param name="onError">Describes what to do if the output fails with
        /// the rest of the other outputs. The default is stop the rest of the
        /// outputs. Possible values include: 'StopProcessingJob',
        /// 'ContinueJob'</param>
        /// <param name="relativePriority">Sets the relative priority of the
        /// TransformOutputs within a Transform. This gives a hint to the
        /// system that one TransformOutput is higher priority than another in
        /// the same Transform. The default is normal. Possible values include:
        /// 'Low', 'Normal', 'High'</param>
        public TransformOutput(Preset preset, OnErrorType? onError = default(OnErrorType?), Priority? relativePriority = default(Priority?))
        {
            OnError = onError;
            RelativePriority = relativePriority;
            Preset = preset;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets describes what to do if the output fails with the rest
        /// of the other outputs. The default is stop the rest of the outputs.
        /// Possible values include: 'StopProcessingJob', 'ContinueJob'
        /// </summary>
        [JsonProperty(PropertyName = "onError")]
        public OnErrorType? OnError { get; set; }

        /// <summary>
        /// Gets or sets sets the relative priority of the TransformOutputs
        /// within a Transform. This gives a hint to the system that one
        /// TransformOutput is higher priority than another in the same
        /// Transform. The default is normal. Possible values include: 'Low',
        /// 'Normal', 'High'
        /// </summary>
        [JsonProperty(PropertyName = "relativePriority")]
        public Priority? RelativePriority { get; set; }

        /// <summary>
        /// Gets or sets preset that describes the Media Processor operation
        /// that will be used to generate the output.
        /// </summary>
        [JsonProperty(PropertyName = "preset")]
        public Preset Preset { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Preset == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Preset");
            }
        }
    }
}