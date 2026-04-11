plugins {
    kotlin("jvm") version "2.3.10"
}

group = "org.m365graphapi"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

dependencies {
    // Include the sdk as a dependency
    implementation("com.microsoft.graph:microsoft-graph:6.+")
    // Include Azure identity for authentication
    implementation("com.azure:azure-identity:1.+")
    // SLF4J logging implementation
    implementation("org.slf4j:slf4j-simple:2.0.16")
    testImplementation(kotlin("test"))
}

kotlin {
    jvmToolchain(21)
}


tasks.test {
    useJUnitPlatform()
}

tasks.withType<JavaExec> {
    jvmArgs("-Dfile.encoding=UTF-8", "-Dstdout.encoding=UTF-8", "-Dstderr.encoding=UTF-8")
}
