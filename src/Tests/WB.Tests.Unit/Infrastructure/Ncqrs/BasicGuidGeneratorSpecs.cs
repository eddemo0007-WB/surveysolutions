﻿using System;
using FluentAssertions;
using Microsoft.Practices.ServiceLocation;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using WB.Tests.Unit;

namespace Ncqrs.Tests
{
    [TestFixture]
    public class BasicGuidGeneratorSpecs
    {
        [SetUp]
        public void SetUp()
        {
            AssemblyContext.SetupServiceLocator();
        }

        [Test]
        public void When_getting_a_new_identifier_it_should_not_be_empty()
        {
            var generator = new BasicGuidGenerator();

            var newIdentifier = generator.GenerateNewId();

            newIdentifier.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void When_getting_a_new_identifier_twice_they_should_not_be_the_same()
        {
            var generator = new BasicGuidGenerator();

            var firstIdentifier = generator.GenerateNewId();
            var secondIdentifier = generator.GenerateNewId();

            firstIdentifier.Should().NotBe(secondIdentifier);
        }

        [Test]
        public void When_getting_a_new_identifier_multiple_times_they_should_all_be_unique()
        {
            var generator = new BasicGuidGenerator();
            var identifiers = new HashSet<Guid>();

            for (int i = 0; i < 500; i++)
            {
                var newId = generator.GenerateNewId();

                identifiers.Should().NotContain(newId);

                identifiers.Add(newId);
            }
        }
    }
}
