[System.AttributeUsage(System.AttributeTargets.Class)]
public class ServerOnlyComponentAttribute : System.Attribute {}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ClientOnlyComponentAttribute : System.Attribute {}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class DevelopmentOnlyComponentAttribute : System.Attribute { }

[System.AttributeUsage(System.AttributeTargets.Class)]
public class EditorOnlyComponentAttribute : System.Attribute {}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class EditorOnlyGameObjectAttribute : System.Attribute {}
